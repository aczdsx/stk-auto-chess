module Fastlane
  module Actions
    module SharedValues
    end

    class ApplyLiappAndroidForUnityAction < Action
      def self.run(params)
        apk, aab = params[:apk], params[:aab]
        UI.user_error!("No apk or aab file given") if apk.to_s.length == 0 && aab.to_s.length == 0

        if apk.to_s.length > 0
          UI.user_error!("Couldn't find apk file at path '#{File.expand_path(apk)}'") unless File.exist?(apk)
        end
        if aab.to_s.length > 0
          UI.user_error!("Couldn't find aab file at path '#{File.expand_path(aab)}'") unless File.exist?(aab)
        end

        liapp_package_sender = params[:liapp_package_sender]
        UI.user_error!("Couldn't find LIAPP package sender file at path '#{File.expand_path(liapp_package_sender)}'") unless File.exist?(liapp_package_sender)

        liapp_env = params[:liapp_package_sender_env]
        UI.user_error!("Couldn't find LIAPP environment file at path '#{File.expand_path(liapp_env)}'") unless File.exist?(liapp_env)

        keystore_file = params[:keystore_file]
        keystore_password = params[:keystore_password]
        keystore_alias = params[:keystore_alias]
        keystore_alias_password = params[:keystore_alias_password]

        UI.user_error!("No keystore file given") if keystore_file.to_s.length == 0
        UI.user_error!("No keystore password given") if keystore_password.to_s.length == 0
        UI.user_error!("No keystore alias given") if keystore_alias.to_s.length == 0
        UI.user_error!("No keystore alias password given") if keystore_alias_password.to_s.length == 0

        if apk.to_s.length > 0
          apply_liapp_and_resign(apk, true, liapp_package_sender, liapp_env, keystore_file, keystore_password, keystore_alias, keystore_alias_password)
        end

        if aab.to_s.length > 0
          apply_liapp_and_resign(aab, false, liapp_package_sender, liapp_env, keystore_file, keystore_password, keystore_alias, keystore_alias_password)
        end
      end

      def self.apply_liapp_and_resign(application, is_apk, liapp_package_sender, liapp_env, ks_file, ks_pass, ks_key_alias, ks_key_alias_pass)
        work_dir, app_basename = File.dirname(application), File.basename(application, ".*")

        # Run LiappPackageSender
        sh("java", "-jar", liapp_package_sender, "-i", application, "-o", "#{application}.liapped", "-e", liapp_env)

        # Since LiappPackageSender always returns exit code 0, check that the output file exists for safety.
        UI.user_error!("Couldn't find LIAPP application result '#{File.expand_path("#{application}.liapped")}'") unless File.exist?("#{application}.liapped")

        FileUtils.mv(application, "#{application}.origin")
        FileUtils.mv("#{application}.liapped", application)

        # Since the content of the application has been changed, we need to re-sign the app.
        # For apk, resign the application by running `zipalign` -> `apksigner`.
        # For aab, resign the application by running `jarsigner`.
        UI.important "Resign the applcation"
        if is_apk
          # See "https://developer.android.com/studio/command-line/zipalign" for more details.
          # Run command:
          #   zipalign -f -v 4 <apk> <apk_zipaligned>
          sh("zipalign", "-f", "-v", "4", application, "#{application}.zipaligned")
          FileUtils.mv("#{application}.zipaligned", application)

          # See "https://developer.android.com/studio/command-line/apksigner" for more details.
          # Run command:
          #   java -jar apksigner.jar sign --ks <keystore-file> --ks-pass <input-format> --ks-key-alias <alias> --key-pass <input-format> <input>
          sh("apksigner", "sign", "--ks", ks_file, "--ks-pass", "pass:#{ks_pass}", "--ks-key-alias", ks_key_alias, "--key-pass", "pass:#{ks_key_alias_pass}", application)
        else
          # See "https://manpages.debian.org/bullseye/openjdk-17-jdk-headless/jarsigner.1.en.html" for more details.
          # Run command:
          #   jarsigner -sigalg <signature-algorithm> -digestalg <digest-algorithm> \
          #     -keystore <keystore-file> -storepass <keystore-pass> -keypass <key-alias-pass> \
          #     <input> <key-alias>
          sh("jarsigner", "-sigalg", "SHA256withRSA", "-digestalg", "SHA-256", "-keystore", ks_file, "-storepass", ks_pass, "-keypass", ks_key_alias_pass, application, ks_key_alias)
        end
      end

      #####################################################
      # @!group Documentation
      #####################################################

      def self.description
        "Apply LIAPP solution and resign the application (Android)"
      end

      def self.details
        "More information: https://docs.tech.cookapps.com/release/com.cookapps.build/manual/case-study/liapp.html"
      end

      def self.available_options
        [
          FastlaneCore::ConfigItem.new(key: :apk,
                                       env_name: '',
                                       description: 'Path to apk file',
                                       optional: true,
                                       default_value: Actions.lane_context[SharedValues::GRADLE_APK_OUTPUT_PATH]),
          FastlaneCore::ConfigItem.new(key: :aab,
                                       env_name: '',
                                       description: 'Path to aab file',
                                       optional: true,
                                       default_value: Actions.lane_context[SharedValues::GRADLE_AAB_OUTPUT_PATH]),
          FastlaneCore::ConfigItem.new(key: :liapp_package_sender,
                                       env_name: 'LIAPP_PACKAGE_SENDER_PATH',
                                       description: 'Path to LiappPackageSender file'),
          FastlaneCore::ConfigItem.new(key: :liapp_package_sender_env,
                                       env_name: 'LIAPP_PACKAGE_SENDER_ENV_PATH',
                                       description: 'Path to environment file used by LiappPackageSender'),
          FastlaneCore::ConfigItem.new(key: :keystore_file,
                                       env_name: 'LIAPP_KEYSTORE_FILE',
                                       description: 'Path to the Keystore file to resign the application'),
          FastlaneCore::ConfigItem.new(key: :keystore_password,
                                       env_name: 'LIAPP_KEYSTORE_PASSWORD',
                                       description: 'Keystore password to resign the application',
                                       sensitive: true),
          FastlaneCore::ConfigItem.new(key: :keystore_alias,
                                       env_name: 'LIAPP_KEYSTORE_ALIAS',
                                       description: 'Keystore key alias to resign the application'),
          FastlaneCore::ConfigItem.new(key: :keystore_alias_password,
                                       env_name: 'LIAPP_KEYSTORE_ALIAS_PASSWORD',
                                       description: 'Keystore key alias password to resign the application',
                                       sensitive: true)
        ]
      end

      def self.output
      end

      def self.return_value
      end

      def self.authors
        ["cookapps-devops"]
      end

      def self.is_supported?(platform)
        [:android].include?(platform)
      end
    end
  end
end
